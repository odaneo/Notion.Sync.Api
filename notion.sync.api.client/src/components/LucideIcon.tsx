import { DynamicIcon } from "lucide-react/dynamic";
import { LucideProps } from "lucide-react";
import dynamicIconImports from "lucide-react/dynamicIconImports";

interface IconProps extends LucideProps {
  name: keyof typeof dynamicIconImports;
}

const LucideIcon = ({ name, ...props }: IconProps) => {
  return (
    <DynamicIcon
      name={Object.hasOwn(dynamicIconImports, name) ? name : "tag"}
      {...props}
    />
  );
};

export default LucideIcon;
